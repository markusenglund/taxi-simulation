from manim import *

DEFAULT_FONT_SIZE = 40
Text.set_default(font="sans-serif", font_size=DEFAULT_FONT_SIZE)

class SurgePriceDiagram(Scene):
    def construct(self):
      self.camera.background_color = "#444444"
      
      axes = Axes(
        x_range=[-1, 1],
        y_range=[0, 1],
        axis_config={"include_tip": True},
        y_axis_config={"include_tip": True, "include_ticks": False},
      ).stretch(0.8, dim=0)
      
      x_label = axes.get_x_axis_label(Text("Excess demand")).scale(0.7)
      x_label2 = Text("Excess supply").next_to(axes, LEFT).shift(2.4*DOWN + 2.1*RIGHT).scale(0.7)
      y_label = axes.get_y_axis_label(Text("Price")).scale(0.7)
      
      surgeGraph = axes.plot(lambda x: 0.2 if x < 0 else 0.2 + 0.7 * (x) ** 2, color=YELLOW)
      self.add(axes)
      self.play(FadeIn(x_label), FadeIn(y_label), FadeIn(x_label2))
      self.play(Create(surgeGraph))
      surgeLabel = Text("Surge Pricing", color=YELLOW).scale(0.7).shift(1.7*UP + 2.2*RIGHT)
      self.play(FadeIn(surgeLabel))
      x = ValueTracker(0)
      dot = always_redraw(lambda: Dot(axes.c2p(x.get_value(), surgeGraph.underlying_function(x.get_value())), color=YELLOW, radius=0.15))
      
      self.play(Create(dot))
      self.play(x.animate.set_value(0.9), run_time=2)
      self.wait(0.5)
      self.play(x.animate.set_value(-0.9), run_time=3)
      self.wait(2)
      self.play(FadeOut(dot))
      staticGraph = axes.plot(lambda x: 0.3, color=ORANGE)
      staticLabel = Text("Static Pricing", color=ORANGE).scale(0.7).to_edge(RIGHT).shift(0.8*DOWN)
      self.play(Create(staticGraph), FadeIn(staticLabel))
      self.wait(4)
