from manim import *

DEFAULT_FONT_SIZE = 17
Text.set_default(font="sans-serif", font_size=DEFAULT_FONT_SIZE)

class SurplusBreakdown(Scene):
    def construct(self):
      self.camera.background_color = "#444444"
      orange = ORANGE
      # values=[0, 0, 0, 0, 0, 0, 0]
      final_values = [-0.24, 4.47, 4.94, 3.06, 2.61, -16.07, -2.61]
      bar_names = ["Total", "Waiting time", "Time sensitivity", "Income", "Substitute speed", "Fare", "# passengers"]      
      # bar_names = ["0", "1", "2", "3", "4", "5",]
      chart = BarChart(
          final_values,
          bar_names=bar_names,
          y_range=[-20, 20, 10],
          y_length=5,
          x_length=12,
          bar_fill_opacity=1,
          y_axis_config={
            "font_size": 24,
            "label_constructor": Text,   
          },
          x_axis_config={
            "font_size": DEFAULT_FONT_SIZE,
            "label_constructor": Text,
            "include_ticks": False,
          },
          bar_colors=[ORANGE, GREEN, GREEN, GREEN, GREEN, ORANGE, ORANGE]
      ).shift(UP*0)
      yLabel = chart.get_y_axis_label(Text("Surplus difference ($)", font_size=26)).shift(LEFT*3.3 + DOWN*3.1).rotate(90 * DEGREES)
      heading = Text("Difference in surplus per passenger", font_size = 40).shift(UP*3.5)
      self.add(heading)
      # axisLabels = chart.get_axis_labels(Text("Available drivers"), Text("Waiting time (minutes)"))
      self.add(chart, yLabel)
      self.add(chart)

      self.wait(2)

      self.play(chart.animate.change_bar_values(final_values), run_time=2)
      def prefix_label(text):
        if "-" not in text:
          return Text("+$" + text)
        return Text("-$" + text.replace("-", ""))

      barLabels = chart.get_bar_labels(font_size=24, label_constructor=lambda text: prefix_label(text))
      self.play(FadeIn(barLabels))
      self.wait(3)
    
