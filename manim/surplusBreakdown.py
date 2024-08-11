from manim import *

DEFAULT_FONT_SIZE = 50
Text.set_default(font="sans-serif", font_size=DEFAULT_FONT_SIZE)
old_values = [-16.07, 4.47, -2.61, 4.94, 2.61, 3.06, -0.24]
excess_demand_values = [-23.46, 7.63, -5.35, 7.65, 3.77, -2.48]
excess_supply_values = [7.10,  -0.67, 5.41, -1.26, -3.02, 7.20]
class SurplusBreakdown(Scene):
    def construct(self):
      self.camera.background_color = "#444444"
      values=[-0.01, 0, -0.01, 0, 0, -0.01]
      bar_names = ["Fare", "Waiting time",  "# passengers","   Time\nsensitivity", "Substitute\n   speed", "Total"]      
      chart = BarChart(
          values,
          bar_names=bar_names,
          y_range=[-25, 25, 5],
          y_length=6,
          x_length=12,
          bar_fill_opacity=1,
          y_axis_config={
            "font_size": 24,
            "label_constructor": Text,   
          },
          x_axis_config={
            "font_size": 21,
            "label_constructor": Text,
            "include_ticks": True,
          },
          bar_colors=[ORANGE, GREEN, ORANGE, GREEN, GREEN, ORANGE]
      ).shift(UP*0)
      heading = Text("Difference in surplus ($/passenger)", font_size = 40).shift(UP*3.5)
      self.add(heading)
      self.add(chart)
      def prefix_label(text):
        if "-" not in text:
          return Text("+$" + text)
        return Text("-$" + text.replace("-", ""))
      def get_bar_labels():
        return chart.get_bar_labels(font_size=24, label_constructor=lambda text: prefix_label(text))
      self.wait(1)
      self.play(chart.animate.change_bar_values(excess_demand_values[0:1]), run_time=1)
      self.play(FadeIn(get_bar_labels()[0:1]))
      self.wait(1)
      self.play(chart.animate.change_bar_values(excess_demand_values[0:2]), run_time=1)
      self.play(FadeIn(get_bar_labels()[0:2]))
      self.wait(1)
      self.play(chart.animate.change_bar_values(excess_demand_values[0:3]), run_time=1)
      self.play(FadeIn(get_bar_labels()[0:3]))
      self.wait(1)
      self.play(chart.animate.change_bar_values(excess_demand_values[0:4]), run_time=1)
      self.play(FadeIn(get_bar_labels()[0:4]))
      self.wait(1)
      self.play(chart.animate.change_bar_values(excess_demand_values[0:5]), run_time=1)
      self.play(FadeIn(get_bar_labels()[0:5]))
      self.wait(1)
      self.play(chart.animate.change_bar_values(excess_demand_values[0:6]), run_time=1)
      self.play(FadeIn(get_bar_labels()[0:6]))
      self.wait(1)
      self.play(chart.animate.change_bar_values(excess_demand_values), run_time=1)
      self.play(FadeIn(get_bar_labels()))

      self.wait(1)

      self.play(chart.animate.change_bar_values(excess_supply_values), run_time=1)
      self.play(FadeIn(get_bar_labels()))
      self.wait(3)
  

class SurplusBreakdownFlip(Scene):
    def construct(self):
      self.camera.background_color = "#444444"
      values=[0, -0.01, 0, -0.01, -0.01, 0]
      bar_names = ["Fare", "Waiting time",  "# passengers","   Time\nsensitivity", "Substitute\n   speed", "Total"]      
      chart = BarChart(
          values,
          bar_names=bar_names,
          y_range=[-25, 25, 5],
          y_length=6,
          x_length=12,
          bar_fill_opacity=1,
          y_axis_config={
            "font_size": 24,
            "label_constructor": Text,   
          },
          x_axis_config={
            "font_size": 21,
            "label_constructor": Text,
            "include_ticks": True,
          },
          bar_colors=[GREEN, ORANGE, GREEN, ORANGE, ORANGE, GREEN]
      ).shift(UP*0)
      heading = Text("Difference in surplus ($/passenger)", font_size = 40).shift(UP*3.5)
      self.add(heading)
      self.add(chart)
      def prefix_label(text):
        if "-" not in text:
          return Text("+$" + text)
        return Text("-$" + text.replace("-", ""))
      def get_bar_labels():
        return chart.get_bar_labels(font_size=24, label_constructor=lambda text: prefix_label(text))
      self.wait(1)
      self.play(chart.animate.change_bar_values(excess_demand_values), run_time=1)

      self.wait(1)

      self.play(chart.animate.change_bar_values(excess_supply_values), run_time=1)
      self.play(FadeIn(get_bar_labels()))
      self.wait(3)